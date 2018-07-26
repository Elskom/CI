require 'sinatra/base'
require 'json'
require 'octokit'

class CI < Sinatra::Base

  # !!! DO NOT EVER USE HARD-CODED VALUES IN A REAL APP !!!
  # Instead, set and test environment variables, like below
  ACCESS_TOKEN = ENV['MY_PERSONAL_TOKEN']

  before do
    @client ||= Octokit::Client.new(:access_token => ACCESS_TOKEN)
  end

  post '/continuous-integration/elskom/ci/pr' do
    @payload = JSON.parse(params[:payload])

    case request.env['HTTP_X_GITHUB_EVENT']
    when "pull_request"
      if @payload["action"] == "opened"
        process_pull_request(@payload["pull_request"])
      end
    end
  end

  helpers do
    def process_pull_request(pull_request)
      @client.create_status(pull_request['base']['repo']['full_name'], pull_request['head']['sha'], 'pending')
      for label in @client.labels_for_issue(pull_request['base']['repo']['full_name'], pull_request['number'])
        if label == "skip news"
          @client.create_status(pull_request['base']['repo']['full_name'], pull_request['head']['sha'], 'success')
          puts "'skip news' label found!"
        end
      end
      for file in @client.pull_request_files(pull_request['base']['repo']['full_name'], pull_request['number'])
        if file.start_with?("Misc/NEWS")
          @client.create_status(pull_request['base']['repo']['full_name'], pull_request['head']['sha'], 'success')
          puts "Misc/NEWS entry found!"
        end
      end
      @client.create_status(pull_request['base']['repo']['full_name'], pull_request['head']['sha'], 'failure')
      puts "Misc/NEWS entry not found and 'skip news' is not added!"
    end
  end
end
