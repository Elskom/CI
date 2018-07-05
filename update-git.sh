# we want latest git.
echo "deb http://ppa.launchpad.net/git-core/ppa/ubuntu trusty main" | sudo tee /etc/apt/sources.list.d/git.list
echo "deb-src http://ppa.launchpad.net/git-core/ppa/ubuntu trusty main" | sudo tee -a /etc/apt/sources.list.d/git.list
sudo apt-get update
sudo apt-get install git
